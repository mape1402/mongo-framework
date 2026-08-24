using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoFramework.Infrastructure.Diagnostics;
using MongoFramework.Infrastructure.Mapping;

namespace MongoFramework.Infrastructure.Linq
{
	public class MongoFrameworkQueryProvider<TEntity> : IMongoFrameworkQueryProvider<TEntity> where TEntity : class
	{
		public IMongoDbConnection Connection { get; }
		private EntityDefinition EntityDefinition { get; }

		private static readonly MethodInfo GenericCreateQueryMethod = typeof(MongoFrameworkQueryProvider<TEntity>).GetRuntimeMethods()
			.Single(m => m.Name == nameof(CreateQuery) && m.IsGenericMethod);

		private BsonDocument PreStage { get; }

		public EntityProcessorCollection<TEntity> EntityProcessors { get; } = new EntityProcessorCollection<TEntity>();

		public MongoFrameworkQueryProvider(IMongoDbConnection connection) : this(connection, null) { }
		public MongoFrameworkQueryProvider(IMongoDbConnection connection, BsonDocument preStage)
		{
			Connection = connection;
			EntityDefinition = EntityMapping.GetOrCreateDefinition(typeof(TEntity));
			PreStage = preStage;
		}
		public MongoFrameworkQueryProvider(IMongoFrameworkQueryProvider<TEntity> provider, BsonDocument preStage) : this(provider.Connection, preStage)
		{
			EntityProcessors.AddRange(provider.EntityProcessors);
		}

		public Expression GetBaseExpression()
		{
			var collection = GetCollection();
			return Expression.Constant(collection.AsQueryable(), typeof(IQueryable<TEntity>));
		}

		public IQueryable CreateQuery(Expression expression)
			=> (IQueryable)GenericCreateQueryMethod
				.MakeGenericMethod(expression.Type.GetSequenceType())
				.Invoke(this, new object[] { expression });

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return new MongoFrameworkQueryable<TElement>(this, expression);
		}

		public object Execute(Expression expression)
		{
			var model = GetExecutionModel(expression);
			var outputType = model.Serializer.ValueType;

			Expression executor = Expression.Call(
				Expression.Constant(this),
				nameof(ExecuteModel),
				new[] { outputType },
				Expression.Constant(model, typeof(AggregateExecutionModel)));

			if (model.ResultTransformer != null)
			{
				executor = Expression.Invoke(model.ResultTransformer, executor);
			}

			var lambda = Expression.Lambda(executor);

			try
			{
				return lambda.Compile().DynamicInvoke(null);
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
		}

		public TResult Execute<TResult>(Expression expression)
		{
			return (TResult)Execute(expression);
		}

		public object ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
		{
			var model = GetExecutionModel(expression, true);
			var outputType = model.Serializer.ValueType;

			//aka. ExecuteModelAsync<outputType>(model, cancellationToken)

			Expression executor = Expression.Call(
				Expression.Constant(this),
				nameof(ExecuteModelAsync),
				new[] { outputType },
				Expression.Constant(model, typeof(AggregateExecutionModel)),
				Expression.Constant(cancellationToken));

			if (model.ResultTransformer != null)
			{
				executor = Expression.Invoke(
					model.ResultTransformer,
					Expression.Convert(executor, model.ResultTransformer.Parameters[0].Type),
					Expression.Constant(cancellationToken)
				);
			}

			var lambda = Expression.Lambda(executor);
			return lambda.Compile().DynamicInvoke(null);
		}

		private IMongoCollection<TEntity> GetCollection()
		{
			return Connection.GetDatabase().GetCollection<TEntity>(EntityDefinition.CollectionName);
		}

		private AggregateExecutionModel GetExecutionModel(Expression expression, bool isAsync = false)
		{
			// Use the official driver to do the heavy lifting on the query translation.
			var rootQueryable = GetCollection().AsQueryable();
			var underlyingProvider = rootQueryable.Provider;
			var providerType = underlyingProvider.GetType(); // Type: MongoQueryProvider<TDocument> (internal)
			var translationOptions = providerType.GetMethod("GetTranslationOptions", BindingFlags.Public | BindingFlags.Instance)
				.Invoke(underlyingProvider, null);

			var hasResultTransformer = HasResultTransformer(expression);
			var pipelineExpression = hasResultTransformer ? GetPipelineExpression(expression) : expression;
			pipelineExpression = new UnderlyingQueryableReplacer(rootQueryable.Expression).Visit(pipelineExpression);
			var outputType = pipelineExpression.Type.GetSequenceType();

			var translatorType = typeof(IMongoCollection<>).Assembly.GetType(
				"MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators.ExpressionToExecutableQueryTranslator",
				throwOnError: true);
			var translateMethod = translatorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(m => m.Name == "Translate"
					&& m.IsGenericMethodDefinition
					&& m.GetGenericArguments().Length == 2);
			var executableQuery = translateMethod
				.MakeGenericMethod(typeof(TEntity), outputType)
				.Invoke(null, new[] { underlyingProvider, pipelineExpression, translationOptions });

			var pipeline = executableQuery.GetType().GetProperty("Pipeline").GetValue(executableQuery);
			var pipelineType = pipeline.GetType();

			var serializer = pipelineType.GetProperty("OutputSerializer").GetValue(pipeline) as IBsonSerializer;
			var ast = pipelineType.GetProperty("Ast").GetValue(pipeline);
			var renderedPipeline = ast.GetType().GetMethod("Render").Invoke(ast, null) as BsonValue;
			var expressionStages = renderedPipeline.AsBsonArray.Cast<BsonDocument>();

			if (PreStage != null)
			{
				expressionStages = new[] { PreStage }.Concat(expressionStages);
			}

			var result = new AggregateExecutionModel
			{
				Stages = expressionStages,
				Serializer = serializer
			};

			// Get the result transforming lambda (allows things like FirstOrDefault, Count, Average etc to work properly).
			if (hasResultTransformer)
			{
				result.ResultTransformer = ResultTransformers.Transform(expression, serializer.ValueType, isAsync) as LambdaExpression;

				// Note: In the future this can change from the initial reflection to a `TryTransform` function where it checks the expression itself.
				//       The reason we are doing this method first is to weed out the bugs and any core missing functionality.
			}

			return result;
		}

		private static Expression GetPipelineExpression(Expression expression)
		{
			if (expression is not MethodCallExpression methodCallExpression)
			{
				return expression;
			}

			if (methodCallExpression.Arguments.Count == 2)
			{
				var sourceExpression = methodCallExpression.Arguments[0];
				var sourceType = sourceExpression.Type.GetSequenceType();
				var argumentExpression = methodCallExpression.Arguments[1];

				return methodCallExpression.Method.Name switch
				{
					nameof(Queryable.First) or
					nameof(Queryable.FirstOrDefault) or
					nameof(Queryable.Single) or
					nameof(Queryable.SingleOrDefault) or
					nameof(Queryable.Count) or
					nameof(Queryable.Any) => Expression.Call(
						null,
						MethodInfoCache.Queryable.Where_2.MakeGenericMethod(sourceType),
						sourceExpression,
						argumentExpression),

					nameof(Queryable.Max) or
					nameof(Queryable.Min) or
					nameof(Queryable.Sum) => Expression.Call(
						null,
						MethodInfoCache.Queryable.Select_2.MakeGenericMethod(sourceType, expression.Type),
						sourceExpression,
						argumentExpression),

					_ => sourceExpression
				};
			}

			return methodCallExpression.Arguments.Count > 0 ? methodCallExpression.Arguments[0] : expression;
		}

		private static bool HasResultTransformer(Expression expression)
		{
			if (expression is not MethodCallExpression methodCallExpression)
			{
				return false;
			}

			return methodCallExpression.Method.Name switch
			{
				nameof(Queryable.First) or
				nameof(Queryable.FirstOrDefault) or
				nameof(Queryable.Single) or
				nameof(Queryable.SingleOrDefault) or
				nameof(Queryable.Count) or
				nameof(Queryable.Max) or
				nameof(Queryable.Min) or
				nameof(Queryable.Sum) or
				nameof(Queryable.Any) => true,
				_ => false
			};
		}

		private IEnumerable<TResult> ExecuteModel<TResult>(AggregateExecutionModel model)
		{
			var serializer = model.Serializer as IBsonSerializer<TResult>;
			var pipeline = PipelineDefinition<TEntity, TResult>.Create(model.Stages, serializer);
			using (var diagnostics = DiagnosticRunner.Start<TEntity>(Connection, model))
			{
				IAsyncCursor<TResult> underlyingCursor;

				try
				{
					underlyingCursor = GetCollection().Aggregate(pipeline);
				}
				catch (Exception exception)
				{
					diagnostics.Error(exception);
					throw;
				}

				var hasFirstResult = false;
				while (underlyingCursor.MoveNext())
				{
					if (!hasFirstResult)
					{
						hasFirstResult = true;
						diagnostics.FirstReadResult<TResult>();
					}

					var resultBatch = underlyingCursor.Current;
					foreach (var item in resultBatch)
					{
						if (item is TEntity entityItem && (model.ResultTransformer == null || model.ResultTransformer.ReturnType == typeof(TEntity)))
						{
							EntityProcessors.ProcessEntity(entityItem, Connection);
						}

						yield return item;
					}
				}
			}
		}

		private async IAsyncEnumerable<TResult> ExecuteModelAsync<TResult>(AggregateExecutionModel model, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var serializer = model.Serializer as IBsonSerializer<TResult>;
			var pipeline = PipelineDefinition<TEntity, TResult>.Create(model.Stages, serializer);

			using (var diagnostics = DiagnosticRunner.Start<TEntity>(Connection, model))
			{
				IAsyncCursor<TResult> underlyingCursor;

				try
				{
					underlyingCursor = await GetCollection().AggregateAsync(pipeline, cancellationToken: cancellationToken);
				}
				catch (Exception exception)
				{
					diagnostics.Error(exception);
					throw;
				}

				var hasFirstResult = false;
				while (await underlyingCursor.MoveNextAsync(cancellationToken))
				{
					if (!hasFirstResult)
					{
						hasFirstResult = true;
						diagnostics.FirstReadResult<TResult>();
					}

					var resultBatch = underlyingCursor.Current;
					foreach (var item in resultBatch)
					{
						if (item is TEntity entityItem &&
							(model.ResultTransformer == null ||
							model.ResultTransformer.ReturnType == typeof(ValueTask<TEntity>) ||
							model.ResultTransformer.ReturnType == typeof(Task<TEntity>)))
						{
							EntityProcessors.ProcessEntity(entityItem, Connection);
						}

						yield return item;
					}
				}
			}
		}

		public string ToQuery(Expression expression)
		{
			var model = GetExecutionModel(expression);
			return QueryHelper.GetQuery<TEntity>(model);
		}

		private class UnderlyingQueryableReplacer : System.Linq.Expressions.ExpressionVisitor
		{
			private readonly Expression Replacement;

			public UnderlyingQueryableReplacer(Expression replacement)
			{
				Replacement = replacement;
			}

			protected override Expression VisitConstant(ConstantExpression node)
			{
				if (node.Value is IQueryable queryable && queryable.Provider is MongoDB.Driver.Linq.IMongoQueryProvider)
				{
					return Replacement;
				}

				return base.VisitConstant(node);
			}
		}
	}
}

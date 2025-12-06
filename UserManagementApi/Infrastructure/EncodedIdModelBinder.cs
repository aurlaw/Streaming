using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UserManagementApi.Infrastructure;

/// <summary>
/// Custom model binder that automatically decodes Base64-encoded IDs.
/// </summary>
public class EncodedIdModelBinder : IModelBinder
{
    private readonly IIdEncoder _encoder;
    
    public EncodedIdModelBinder(IIdEncoder encoder)
    {
        _encoder = encoder;
    }
    
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
        
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }
        
        var encodedValue = valueProviderResult.FirstValue;
        
        if (string.IsNullOrEmpty(encodedValue))
        {
            return Task.CompletedTask;
        }
        
        // Determine the type prefix from the route path
        var path = bindingContext.ActionContext.HttpContext.Request.Path.Value ?? "";
        var prefix = EntityIds.GetPrefixFromRoute(path);
        
        if (string.IsNullOrEmpty(prefix))
        {
            bindingContext.ModelState.AddModelError(
                modelName,
                "Cannot determine ID type from route");
            return Task.CompletedTask;
        }
        
        // Decode based on target type
        var targetType = bindingContext.ModelType;
        
        if (targetType == typeof(int))
        {
            var result = _encoder.DecodeInt(encodedValue, prefix);
            
            if (result is Domain.Result<int, Domain.Error>.Success(var id))
            {
                bindingContext.Result = ModelBindingResult.Success(id);
            }
            else if (result is Domain.Result<int, Domain.Error>.Failure(var error))
            {
                bindingContext.ModelState.AddModelError(
                    modelName,
                    GetErrorMessage(error));
            }
        }
        else if (targetType == typeof(long))
        {
            var result = _encoder.DecodeLong(encodedValue, prefix);
            
            if (result is Domain.Result<long, Domain.Error>.Success(var id))
            {
                bindingContext.Result = ModelBindingResult.Success(id);
            }
            else if (result is Domain.Result<long, Domain.Error>.Failure(var error))
            {
                bindingContext.ModelState.AddModelError(
                    modelName,
                    GetErrorMessage(error));
            }
        }
        else if (targetType == typeof(Guid))
        {
            var result = _encoder.DecodeGuid(encodedValue, prefix);
            
            if (result is Domain.Result<Guid, Domain.Error>.Success(var id))
            {
                bindingContext.Result = ModelBindingResult.Success(id);
            }
            else if (result is Domain.Result<Guid, Domain.Error>.Failure(var error))
            {
                bindingContext.ModelState.AddModelError(
                    modelName,
                    GetErrorMessage(error));
            }
        }
        
        return Task.CompletedTask;
    }
    
    private string GetErrorMessage(Domain.Error error)
    {
        return error switch
        {
            Domain.Error.ValidationError(var msg) => msg,
            _ => "Invalid ID format"
        };
    }
}

/// <summary>
/// Model binder provider for encoded IDs.
/// </summary>
public class EncodedIdModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(int) ||
            context.Metadata.ModelType == typeof(long) ||
            context.Metadata.ModelType == typeof(Guid))
        {
            // Check if this is an ID parameter by name convention
            var parameterName = context.Metadata.ParameterName?.ToLower();
            if (parameterName == "id")
            {
                var encoder = context.Services.GetRequiredService<IIdEncoder>();
                return new EncodedIdModelBinder(encoder);
            }
        }
        
        return null;
    }
}
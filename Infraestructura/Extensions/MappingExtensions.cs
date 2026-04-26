namespace Infraestructura.Extensions
{
    public static class MappingExtensions
    {
        /// <summary>
        /// Mapea automáticamente las propiedades con el mismo nombre y tipo
        /// entre un modelo y su DTO, sin necesidad de implementar ninguna interfaz.
        /// </summary>
        public static TDto ToDto<TDto, TModel>(this TModel model)
            where TDto : new()
        {
            var dto = new TDto();

            var modelProps = typeof(TModel).GetProperties();
            var dtoProps = typeof(TDto).GetProperties()
                                         .ToDictionary(p => p.Name);

            foreach (var modelProp in modelProps)
            {
                // Si el DTO tiene una propiedad con el mismo nombre
                if (!dtoProps.TryGetValue(modelProp.Name, out var dtoProp))
                    continue;

                // Si el DTO puede escribirse y los tipos son compatibles
                if (!dtoProp.CanWrite)
                    continue;

                var value = modelProp.GetValue(model);

                // Tipos iguales → asigna directo
                if (dtoProp.PropertyType == modelProp.PropertyType)
                {
                    dtoProp.SetValue(dto, value);
                }
                // Tipos Nullable compatibles → desenvuelve si es necesario
                else if (value != null &&
                         Nullable.GetUnderlyingType(dtoProp.PropertyType) == modelProp.PropertyType)
                {
                    dtoProp.SetValue(dto, value);
                }
            }

            return dto;
        }

        /// <summary>
        /// Mapea una colección completa automáticamente.
        /// </summary>
        public static IEnumerable<TDto> ToDtoList<TDto, TModel>(this IEnumerable<TModel> models)
            where TDto : new()
            => models.Select(m => m.ToDto<TDto, TModel>());

        /// <summary>
        /// Mapea de vuelta al modelo original con la misma lógica.
        /// </summary>
        public static TModel ToOriginal<TDto, TModel>(this TDto dto)
            where TModel : new()
            => dto.ToDto<TModel, TDto>(); // misma lógica, tipos invertidos
    }
}
namespace RedDust.Properties
{
    /// <summary>属性只读接口。外部系统通过此接口读取属性值，无权修改。</summary>
    public interface IPropertyReader
    {
        float GetFloat(string path);
        float GetMax(string path);
        bool Has(string path);
    }
}

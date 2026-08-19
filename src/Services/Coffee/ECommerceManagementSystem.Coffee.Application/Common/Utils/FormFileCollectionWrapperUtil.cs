namespace ECommerceManagementSystem.Coffee.Application.Common.Utils;

public class FormFileCollectionWrapperUtil : List<IFormFile>, IFormFileCollection
{
    public FormFileCollectionWrapperUtil(IEnumerable<IFormFile> files)
        : base(files) { }

    public IFormFile? this[string name] =>
        this.FirstOrDefault(f =>
            f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public IFormFile? GetFile(string name)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<IFormFile> GetFiles(string name) =>
        this.Where(f =>
            f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
}
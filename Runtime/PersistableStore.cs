using Persistence;
using SaveManagement;

namespace Stores
{
  public abstract class PersistableStoreSO<T> : StoreSO, ISaveable<T> where T : SaveData, new()
  {
    public abstract string FileName { get; }
    public abstract void Load(T data);
    public abstract void Save(T data);
    public abstract override void SetToDefaultValues();

    public abstract bool AutoSave { get; }
    public abstract bool AutoLoad { get; }
    protected bool Persisted => SaveManager.SaveableExists<PersistableStoreSO<T>, T>(this);

    protected void Awake()
    {
      if (AutoLoad)
      {
        LoadFromFileOrDefaultValues();
      }
    }

    protected void OnDisable()
    {
      if (AutoSave)
      {
        Save();
      }
    }

    public void LoadFromFileOrDefaultValues()
    {
      if (JsonPersistence.JsonExists(FileName))
      {
        Load();
      }
      else
      {
        SetToDefaultValues();
      }
    }

    public async void Save(string fileName = null)
    {
      await SaveManager.SaveSaveable<PersistableStoreSO<T>, T>(this, fileName);
    }

    public async void Load(string fileName = null)
    {
      await SaveManager.LoadSaveable<PersistableStoreSO<T>, T>(this, fileName);
    }

    public void Delete(string fileName = null)
    {
      string file = fileName != null ? fileName : FileName;
      JsonPersistence.DeleteJson(file);
    }
  }
}
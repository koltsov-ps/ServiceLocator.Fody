![Icon](https://raw.github.com/Fody/BasicFodyAddin/master/Icons/package_icon.png)

Это библиотека для генерации ServiceLocator’а на этапе компиляции. Реализована с использованием [Fody](https://github.com/Fody/Fody) /Библиотека находится в разработке, и пока не предназначена для использования в продакшене./
 
## Что такое ServiceLocator?
ServiceLocator — это поражающий паттерн следующего вида:

```
public class ServiceLocator
{
	IServiceA a;
	public IServiceA ServiceA 
		=> a ?? (a = new ServiceA(ServiceB));

	IServiceB b;
	public IServiceB ServiceB
		=> b ?? (b = new ServiceB());
}
```

Это класс, который лениво создает дерево объектов таким образом, чтобы каждый объект был создан не более одного раза. В приведенном выше примере при создании ServiceA используется созданный или создается новый объект ServiceB.

## А почему не Dependency Injection контейнер?
ServiceLocator не требует конфигурирования на старте приложения. Все что требуется для его работы уже посчитано и скомпилировано.
Этот паттерн прекрасно подходит для создания мобильных приложений на Xamarin, где время старта приложения является очень важным параметром. Если требуется скорость запуска приложения и не требуется гибкая настройка, то ServiceLocator подходит хорошо.

## Как подключить к проекту?
Через [NuGet](https://www.nuget.org/packages/ServiceLocator.Fody/)

## Как использовать?
Использовать ServiceLocator.Fody предлагается следующим образом:
1. Описать интерфейс на основе, которого будет сгененрировано дерево зависимостей. 
2. Описать класс, в котором надо будет сгенерировать код реализации интерфейса.

Например так:
```
public interface IServiceLocator
{
	IRepository Repository { get; }
}

[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator {
	private IServiceLocator instance;
	public static IServiceLocator Instance
		=> instance ?? (instance = (IServiceLocator) new ServiceLocator());
}

//Теперь для создания репозитория в основной программе делаем так:
var repo = ServiceLocator.Instance.Repository;
```

## Возможности

### Создание Singleton объекта через определение свойства или метода
```
public interface IServiceLocator {
	IService1 Service1 { get; }
  IService2 GetService2();
}
```

### Создание нового экземпляра класса, при каждом вызове, если название метода начинается с Create
```
public interface IServiceLocator {
  IService CreateService();
}
```

### При создании нового экземпляра посредством Create-метода, можно передавать аргументы для конструктора.
```
public class Database : IRepository {
	public Database (string path) {
		//…
	}
}

public interface IServiceLocator {
	IRepository CreateRepository(string path);
}
```
Если для создания экземпляра класса требуются параметры, которые можно получить только с наружи, то их следует передать в качестве параметров в Create-метод. При этом имена параметров в конструкторе класса и в сигнатуре метода должны совпадать. В данном примере для создания класса Database нужен параметр path. Его следует передать в метод CreateRepository.

### Если в дереве объектов есть сложный объект и есть необходимость написать специальный код для его создания, то следует это сделать в классе, реализующем интерфейс ServiceLocator’а.
```
public class IServiceLocator {
	…
}
[ImplementServiceLocator(typeof(IServiceLocator))]
public class ServiceLocator {
	private static Database CreateDatabase() {
		…
		return new Database(…);
	} 
}
```
Если при создании дерева объектов понадобится создать экземпляр Database, то для этого будет вызван метод ServiceLocator.CreateDatabase.

## Icon

<a href="http://thenounproject.com/noun/lego/#icon-No16919" target="_blank">Lego</a> designed by <a href="http://thenounproject.com/timur.zima" target="_blank">Timur Zima</a> from The Noun Project

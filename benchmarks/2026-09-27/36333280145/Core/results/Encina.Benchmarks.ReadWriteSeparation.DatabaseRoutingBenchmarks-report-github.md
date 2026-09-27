```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                 | Mean       | Error       | StdDev      | Median     | Allocated |
|--------------------------------------- |-----------:|------------:|------------:|-----------:|----------:|
| &#39;Nested scopes (Read → ForceWrite)&#39;    | 4,964.5 ns |  4,571.9 ns |   250.60 ns | 4,944.5 ns |     840 B |
| &#39;Read CurrentIntent (AsyncLocal)&#39;      | 1,099.7 ns | 28,566.8 ns | 1,565.84 ns |   241.0 ns |         - |
| DatabaseRoutingScope.ForRead()         | 4,455.3 ns |  8,089.6 ns |   443.42 ns | 4,359.0 ns |     392 B |
| &#39;Read EffectiveIntent (null-coalesce)&#39; |   173.3 ns |  1,004.8 ns |    55.08 ns |   170.0 ns |         - |
| &#39;Read HasIntent&#39;                       |   201.3 ns |    946.3 ns |    51.87 ns |   215.0 ns |         - |
| &#39;Read IsReadIntent&#39;                    |   215.5 ns |    632.0 ns |    34.64 ns |   235.5 ns |         - |
| &#39;Read IsWriteIntent&#39;                   |   242.0 ns |  1,000.4 ns |    54.84 ns |   216.0 ns |         - |
| DatabaseRoutingScope.ForWrite()        | 3,960.0 ns |  3,041.1 ns |   166.69 ns | 3,906.0 ns |     392 B |
| DatabaseRoutingScope.ForForceWrite()   | 3,508.7 ns |  5,942.5 ns |   325.73 ns | 3,395.0 ns |     392 B |
| DatabaseRoutingContext.Clear()         | 2,158.0 ns |  1,476.2 ns |    80.91 ns | 2,144.0 ns |      96 B |

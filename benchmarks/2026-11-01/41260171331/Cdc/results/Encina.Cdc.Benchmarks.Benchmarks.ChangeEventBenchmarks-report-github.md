```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 24.077 ns | 0.2365 ns | 0.3466 ns |  0.61 |    0.05 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.388 ns | 0.0026 ns | 0.0037 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.687 ns | 0.1153 ns | 0.1726 ns |  0.35 |    0.03 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 39.621 ns | 2.1227 ns | 3.1771 ns |  1.01 |    0.11 | 0.0033 |      56 B |        1.00 |

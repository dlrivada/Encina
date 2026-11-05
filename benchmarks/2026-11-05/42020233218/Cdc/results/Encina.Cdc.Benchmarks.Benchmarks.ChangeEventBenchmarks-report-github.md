```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.573 ns | 3.1905 ns | 0.1749 ns |  0.65 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.227 ns | 0.0629 ns | 0.0034 ns |  0.03 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.900 ns | 1.1837 ns | 0.0649 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.252 ns | 1.4108 ns | 0.0773 ns |  1.00 | 0.0033 |      56 B |        1.00 |

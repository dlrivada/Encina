```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.434 ns | 4.2629 ns | 0.2337 ns |  0.63 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.397 ns | 0.2314 ns | 0.0127 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.829 ns | 2.3549 ns | 0.1291 ns |  0.36 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.643 ns | 1.1135 ns | 0.0610 ns |  1.00 | 0.0033 |      56 B |        1.00 |

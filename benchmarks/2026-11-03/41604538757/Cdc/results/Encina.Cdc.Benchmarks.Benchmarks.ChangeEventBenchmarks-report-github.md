```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 24.237 ns | 7.2509 ns | 0.3974 ns |  0.68 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.410 ns | 0.1484 ns | 0.0081 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.396 ns | 3.6010 ns | 0.1974 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.669 ns | 3.9181 ns | 0.2148 ns |  1.00 | 0.0033 |      56 B |        1.00 |

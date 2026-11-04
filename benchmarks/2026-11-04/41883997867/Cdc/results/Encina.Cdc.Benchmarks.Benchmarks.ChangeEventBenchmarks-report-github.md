```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.437 ns | 3.4067 ns | 0.1867 ns |  0.62 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.421 ns | 1.0243 ns | 0.0561 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.103 ns | 0.7220 ns | 0.0396 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.851 ns | 2.7006 ns | 0.1480 ns |  1.00 | 0.0033 |      56 B |        1.00 |

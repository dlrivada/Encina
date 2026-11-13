```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.109 ns | 2.7207 ns | 0.1491 ns |  0.62 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.385 ns | 0.0718 ns | 0.0039 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.868 ns | 2.9329 ns | 0.1608 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 34.322 ns | 1.6921 ns | 0.0927 ns |  1.00 | 0.0033 |      56 B |        1.00 |

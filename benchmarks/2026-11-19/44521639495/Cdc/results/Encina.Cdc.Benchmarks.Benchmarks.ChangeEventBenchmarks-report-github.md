```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.307 ns | 2.0807 ns | 0.1141 ns |  0.64 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.403 ns | 0.1365 ns | 0.0075 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.817 ns | 4.5963 ns | 0.2519 ns |  0.37 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.021 ns | 6.4559 ns | 0.3539 ns |  1.00 | 0.0033 |      56 B |        1.00 |

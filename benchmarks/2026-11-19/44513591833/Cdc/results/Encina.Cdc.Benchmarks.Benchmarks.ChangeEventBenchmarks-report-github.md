```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 21.765 ns | 4.3703 ns | 0.2396 ns |  0.61 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.398 ns | 0.3240 ns | 0.0178 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.685 ns | 5.2103 ns | 0.2856 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.737 ns | 2.4835 ns | 0.1361 ns |  1.00 | 0.0033 |      56 B |        1.00 |

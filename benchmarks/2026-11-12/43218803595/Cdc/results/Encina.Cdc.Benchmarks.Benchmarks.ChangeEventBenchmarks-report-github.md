```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 22.558 ns | 0.7948 ns | 0.0436 ns |  0.63 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.383 ns | 0.0018 ns | 0.0001 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.573 ns | 1.6727 ns | 0.0917 ns |  0.35 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.723 ns | 6.8852 ns | 0.3774 ns |  1.00 | 0.0033 |      56 B |        1.00 |

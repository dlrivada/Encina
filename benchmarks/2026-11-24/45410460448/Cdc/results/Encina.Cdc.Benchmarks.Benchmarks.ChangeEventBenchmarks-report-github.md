```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.7746 ns | 2.9328 ns | 0.1608 ns |  0.67 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9867 ns | 0.1808 ns | 0.0099 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.6645 ns | 2.6385 ns | 0.1446 ns |  0.38 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.0079 ns | 1.0399 ns | 0.0570 ns |  1.00 | 0.0033 |      56 B |        1.00 |

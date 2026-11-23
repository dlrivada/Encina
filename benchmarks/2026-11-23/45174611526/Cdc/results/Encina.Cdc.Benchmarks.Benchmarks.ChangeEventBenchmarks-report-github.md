```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 18.3822 ns | 2.8176 ns | 0.1544 ns |  0.65 |    0.01 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.9924 ns | 0.2471 ns | 0.0135 ns |  0.03 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 11.0517 ns | 3.5026 ns | 0.1920 ns |  0.39 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 28.4271 ns | 6.4575 ns | 0.3540 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |

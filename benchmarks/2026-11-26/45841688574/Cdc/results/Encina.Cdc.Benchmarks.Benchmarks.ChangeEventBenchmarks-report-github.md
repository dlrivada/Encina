```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 19.070 ns |  2.1830 ns | 0.1197 ns |  0.57 |    0.01 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.454 ns |  0.0190 ns | 0.0010 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 10.340 ns |  4.2533 ns | 0.2331 ns |  0.31 |    0.01 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 33.414 ns | 12.1322 ns | 0.6650 ns |  1.00 |    0.02 | 0.0007 |      56 B |        1.00 |

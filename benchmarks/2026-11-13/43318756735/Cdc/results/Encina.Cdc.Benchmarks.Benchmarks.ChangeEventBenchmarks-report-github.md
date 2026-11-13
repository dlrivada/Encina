```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateChangeEvent          | 29.126 ns | 3.7560 ns | 0.2059 ns |  0.81 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.577 ns | 0.0040 ns | 0.0002 ns |  0.04 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 14.102 ns | 1.3328 ns | 0.0731 ns |  0.39 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 35.958 ns | 0.9307 ns | 0.0510 ns |  1.00 | 0.0007 |      56 B |        1.00 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 25.206 ns | 22.6495 ns | 1.2415 ns |  0.75 |    0.03 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.546 ns |  0.8067 ns | 0.0442 ns |  0.05 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 12.995 ns |  6.9102 ns | 0.3788 ns |  0.38 |    0.01 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 33.796 ns |  7.8315 ns | 0.4293 ns |  1.00 |    0.02 | 0.0007 |      56 B |        1.00 |

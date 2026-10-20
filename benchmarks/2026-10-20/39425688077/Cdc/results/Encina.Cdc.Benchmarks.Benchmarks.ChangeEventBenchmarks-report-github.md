```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 13.9233 ns |  6.9477 ns | 0.3808 ns |  0.53 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.3730 ns |  0.4928 ns | 0.0270 ns |  0.01 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  8.8085 ns | 14.7408 ns | 0.8080 ns |  0.34 |    0.03 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 26.2629 ns | 10.0803 ns | 0.5525 ns |  1.00 |    0.03 | 0.0033 |      56 B |        1.00 |

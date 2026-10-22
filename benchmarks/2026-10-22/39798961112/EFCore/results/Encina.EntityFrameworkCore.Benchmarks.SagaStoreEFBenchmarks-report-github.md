```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method             | Mean      | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|--------:|----------:|------------:|
| GetAsync           |  86.72 μs | 3.240 μs | 4.542 μs |  1.00 |    0.07 |  10.64 KB |        1.00 |
| AddAsync           |  27.05 μs | 1.764 μs | 2.473 μs |  0.31 |    0.03 |   3.27 KB |        0.31 |
| UpdateAsync        |  38.63 μs | 1.944 μs | 2.726 μs |  0.45 |    0.04 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 734.40 μs | 4.658 μs | 6.377 μs |  8.49 |    0.40 | 335.49 KB |       31.53 |

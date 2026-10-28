```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           |   168.17 μs | 255.91 μs | 14.027 μs |  1.00 |    0.10 |  10.64 KB |        1.00 |
| AddAsync           |    49.07 μs |  84.79 μs |  4.648 μs |  0.29 |    0.03 |   3.27 KB |        0.31 |
| UpdateAsync        |    81.11 μs | 208.69 μs | 11.439 μs |  0.48 |    0.07 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 1,015.51 μs | 378.16 μs | 20.728 μs |  6.07 |    0.43 | 335.49 KB |       31.53 |

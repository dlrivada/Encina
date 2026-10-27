```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|------------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           | 134.86 μs |   406.10 μs | 22.260 μs |  1.02 |    0.22 |  10.64 KB |        1.00 |
| AddAsync           |  41.95 μs |   164.59 μs |  9.022 μs |  0.32 |    0.08 |   3.27 KB |        0.31 |
| UpdateAsync        |  79.62 μs |   385.80 μs | 21.147 μs |  0.60 |    0.17 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 821.33 μs | 1,109.14 μs | 60.796 μs |  6.21 |    1.05 |  335.8 KB |       31.56 |

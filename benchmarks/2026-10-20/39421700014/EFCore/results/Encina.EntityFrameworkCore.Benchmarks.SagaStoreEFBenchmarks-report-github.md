```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           |   114.13 μs |  58.22 μs |  3.191 μs |  1.00 |    0.03 |  10.64 KB |        1.00 |
| AddAsync           |    52.74 μs | 203.95 μs | 11.179 μs |  0.46 |    0.09 |   3.27 KB |        0.31 |
| UpdateAsync        |    58.49 μs |  30.40 μs |  1.666 μs |  0.51 |    0.02 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 1,000.39 μs | 330.88 μs | 18.137 μs |  8.77 |    0.25 | 335.49 KB |       31.53 |

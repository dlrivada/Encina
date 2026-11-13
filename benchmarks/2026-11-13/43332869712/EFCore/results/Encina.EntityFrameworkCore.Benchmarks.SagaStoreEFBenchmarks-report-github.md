```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean        | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |------------:|----------:|---------:|------:|--------:|----------:|------------:|
| GetAsync           |   114.41 μs |  77.94 μs | 4.272 μs |  1.00 |    0.05 |  10.64 KB |        1.00 |
| AddAsync           |    56.67 μs | 159.05 μs | 8.718 μs |  0.50 |    0.07 |   3.27 KB |        0.31 |
| UpdateAsync        |    64.61 μs |  95.64 μs | 5.242 μs |  0.57 |    0.04 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 1,000.80 μs |  82.45 μs | 4.519 μs |  8.76 |    0.29 | 335.49 KB |       31.53 |

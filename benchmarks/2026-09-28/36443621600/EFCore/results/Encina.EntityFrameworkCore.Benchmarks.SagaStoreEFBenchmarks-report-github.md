```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetAsync           | 131.94 μs | 122.17 μs |  6.697 μs |  1.00 |    0.06 |  10.64 KB |        1.00 |
| AddAsync           |  51.05 μs |  52.70 μs |  2.889 μs |  0.39 |    0.03 |   3.27 KB |        0.31 |
| UpdateAsync        |  61.09 μs |  66.05 μs |  3.620 μs |  0.46 |    0.03 |   5.64 KB |        0.53 |
| GetStuckSagasAsync | 989.14 μs | 409.48 μs | 22.445 μs |  7.51 |    0.37 | 335.49 KB |       31.53 |

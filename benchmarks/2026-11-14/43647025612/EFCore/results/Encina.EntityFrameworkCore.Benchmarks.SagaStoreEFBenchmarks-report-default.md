
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 GetAsync           | 189.63 μs | 314.58 μs | 17.243 μs |  1.01 |    0.11 |  10.64 KB |        1.00 |
 AddAsync           |  83.57 μs | 155.92 μs |  8.546 μs |  0.44 |    0.05 |   3.27 KB |        0.31 |
 UpdateAsync        |  92.12 μs |  76.51 μs |  4.194 μs |  0.49 |    0.04 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 989.72 μs | 693.25 μs | 37.999 μs |  5.25 |    0.43 | 335.49 KB |       31.53 |

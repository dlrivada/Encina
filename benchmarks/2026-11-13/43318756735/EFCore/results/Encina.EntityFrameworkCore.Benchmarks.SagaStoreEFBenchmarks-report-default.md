
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
 GetAsync           | 116.08 μs | 104.57 μs | 5.732 μs |  1.00 |    0.06 |  10.64 KB |        1.00 |
 AddAsync           |  56.84 μs | 181.88 μs | 9.970 μs |  0.49 |    0.08 |   3.27 KB |        0.31 |
 UpdateAsync        |  59.41 μs |  20.37 μs | 1.117 μs |  0.51 |    0.02 |   5.64 KB |        0.53 |
 GetStuckSagasAsync | 983.79 μs |  59.67 μs | 3.271 μs |  8.49 |    0.36 | 335.49 KB |       31.53 |

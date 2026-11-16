```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Record: track entity&#39;               | 3.029 μs | 0.7526 μs | 0.0413 μs |  1.00 |    0.02 |    2 | 0.0420 | 0.0191 |    1088 B |        1.00 |
| &#39;Record: mark expired&#39;               | 1.903 μs | 0.1422 μs | 0.0078 μs |  0.63 |    0.01 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
| &#39;Record: mark deleted (terminal)&#39;    | 1.878 μs | 0.4365 μs | 0.0239 μs |  0.62 |    0.01 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |
| &#39;Record: mark anonymized (terminal)&#39; | 1.852 μs | 0.2227 μs | 0.0122 μs |  0.61 |    0.01 |    1 | 0.0229 | 0.0210 |     608 B |        0.56 |

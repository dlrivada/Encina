```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Approve assessment&#39; | 2.252 μs | 0.8505 μs | 0.0466 μs |  1.00 |    0.03 |    2 | 0.0343 | 0.0305 |     944 B |        1.00 |
| &#39;Reject assessment&#39;  | 1.366 μs | 0.2599 μs | 0.0142 μs |  0.61 |    0.01 |    1 | 0.0134 | 0.0114 |     368 B |        0.39 |
| &#39;Request revision&#39;   | 1.263 μs | 0.3542 μs | 0.0194 μs |  0.56 |    0.01 |    1 | 0.0134 | 0.0114 |     368 B |        0.39 |
| &#39;Expire assessment&#39;  | 1.128 μs | 0.2988 μs | 0.0164 μs |  0.50 |    0.01 |    1 | 0.0134 | 0.0114 |     352 B |        0.37 |

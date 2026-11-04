```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 2.242 μs | 0.9623 μs | 0.0527 μs |  1.00 |    0.03 |    3 | 0.0381 | 0.0191 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1.844 μs | 0.3659 μs | 0.0201 μs |  0.82 |    0.02 |    2 | 0.0267 | 0.0248 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; | 1.168 μs | 0.3079 μs | 0.0169 μs |  0.52 |    0.01 |    1 | 0.0229 | 0.0210 |     576 B |        0.60 |

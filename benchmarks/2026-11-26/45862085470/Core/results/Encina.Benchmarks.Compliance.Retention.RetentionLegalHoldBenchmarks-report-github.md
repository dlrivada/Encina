```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 2.279 μs | 0.4729 μs | 0.0259 μs |  1.00 |    0.01 |    3 | 0.0114 | 0.0076 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1.844 μs | 0.6449 μs | 0.0354 μs |  0.81 |    0.02 |    2 | 0.0076 | 0.0057 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; | 1.126 μs | 0.2282 μs | 0.0125 μs |  0.49 |    0.01 |    1 | 0.0057 | 0.0038 |     576 B |        0.60 |

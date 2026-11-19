```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                         | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Policy: create (fast path)&#39;                   | 2.156 μs | 0.7456 μs | 0.0409 μs |  1.00 |    0.02 |    4 | 0.0134 | 0.0114 |    1248 B |        1.00 |
| &#39;Policy: get retention period (cached lookup)&#39; | 1.010 μs | 0.2591 μs | 0.0142 μs |  0.47 |    0.01 |    1 | 0.0076 | 0.0057 |     656 B |        0.53 |
| &#39;Policy: get by ID&#39;                            | 1.731 μs | 0.4132 μs | 0.0226 μs |  0.80 |    0.02 |    3 | 0.0095 | 0.0076 |     864 B |        0.69 |
| &#39;Policy: deactivate&#39;                           | 1.352 μs | 0.1828 μs | 0.0100 μs |  0.63 |    0.01 |    2 | 0.0076 | 0.0057 |     680 B |        0.54 |

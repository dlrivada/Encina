```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 1,816.2 ns | 1,256.4 ns | 68.87 ns |  1.00 |    0.05 |    3 | 0.0114 | 0.0095 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,451.8 ns |   724.0 ns | 39.69 ns |  0.80 |    0.03 |    2 | 0.0076 | 0.0057 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; |   930.0 ns |   391.8 ns | 21.47 ns |  0.51 |    0.02 |    1 | 0.0067 | 0.0057 |     592 B |        0.62 |

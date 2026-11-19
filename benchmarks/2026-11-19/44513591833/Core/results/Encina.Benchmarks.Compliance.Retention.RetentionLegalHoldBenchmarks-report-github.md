```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.56GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 1,866.8 ns | 637.6 ns | 34.95 ns |  1.00 |    0.02 |    3 | 0.0114 | 0.0095 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,527.5 ns | 275.5 ns | 15.10 ns |  0.82 |    0.02 |    2 | 0.0076 | 0.0057 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; |   918.4 ns | 227.9 ns | 12.49 ns |  0.49 |    0.01 |    1 | 0.0067 | 0.0057 |     592 B |        0.62 |

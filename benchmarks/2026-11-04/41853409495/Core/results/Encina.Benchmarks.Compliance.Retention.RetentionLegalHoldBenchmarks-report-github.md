```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 1,826.1 ns | 846.6 ns | 46.40 ns |  1.00 |    0.03 |    3 | 0.0114 | 0.0076 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,468.2 ns | 387.3 ns | 21.23 ns |  0.80 |    0.02 |    2 | 0.0076 | 0.0057 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; |   981.2 ns | 190.7 ns | 10.45 ns |  0.54 |    0.01 |    1 | 0.0067 | 0.0057 |     592 B |        0.62 |

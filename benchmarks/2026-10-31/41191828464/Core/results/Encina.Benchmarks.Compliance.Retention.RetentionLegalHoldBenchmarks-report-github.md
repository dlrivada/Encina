```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error     | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|----------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 1,991.5 ns |  39.25 ns |  2.15 ns |  1.00 |    3 | 0.0114 | 0.0076 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,477.6 ns | 224.60 ns | 12.31 ns |  0.74 |    2 | 0.0076 | 0.0057 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; |   961.1 ns | 544.93 ns | 29.87 ns |  0.48 |    1 | 0.0067 | 0.0057 |     592 B |        0.62 |

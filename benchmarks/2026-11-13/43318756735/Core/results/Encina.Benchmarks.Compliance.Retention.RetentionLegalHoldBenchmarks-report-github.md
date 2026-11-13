```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 1,850.2 ns | 833.7 ns | 45.70 ns |  1.00 |    0.03 |    2 | 0.0572 | 0.0286 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,584.9 ns | 395.9 ns | 21.70 ns |  0.86 |    0.02 |    2 | 0.0401 | 0.0191 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; |   720.7 ns | 201.9 ns | 11.06 ns |  0.39 |    0.01 |    1 | 0.0343 | 0.0172 |     592 B |        0.62 |

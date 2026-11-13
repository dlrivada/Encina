```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 1,586.0 ns | 336.9 ns | 18.47 ns |  1.00 |    0.01 |    2 | 0.0572 | 0.0286 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,426.5 ns | 597.7 ns | 32.76 ns |  0.90 |    0.02 |    2 | 0.0401 | 0.0191 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; |   597.9 ns | 187.8 ns | 10.29 ns |  0.38 |    0.01 |    1 | 0.0343 | 0.0172 |     592 B |        0.62 |

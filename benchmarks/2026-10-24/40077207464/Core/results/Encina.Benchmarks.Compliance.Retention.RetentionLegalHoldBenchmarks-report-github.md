```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                     | Mean       | Error    | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------- |-----------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Legal hold: place (cross-aggregate)&#39;      | 2,152.5 ns | 168.8 ns |  9.25 ns |  1.00 |    3 | 0.0572 | 0.0267 |     960 B |        1.00 |
| &#39;Legal hold: lift&#39;                         | 1,731.1 ns | 364.3 ns | 19.97 ns |  0.80 |    2 | 0.0401 | 0.0191 |     680 B |        0.71 |
| &#39;Legal hold: has active holds (read-only)&#39; |   925.2 ns | 425.1 ns | 23.30 ns |  0.43 |    1 | 0.0343 | 0.0172 |     592 B |        0.62 |

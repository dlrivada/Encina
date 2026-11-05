```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Validate: adequacy decision (fast path)&#39; | 1.890 μs | 0.6851 μs | 0.0376 μs |  1.00 |    0.02 |    1 | 0.0591 | 0.0286 |     992 B |        1.00 |
| &#39;Validate: approved transfer&#39;             | 3.763 μs | 1.6523 μs | 0.0906 μs |  1.99 |    0.05 |    2 | 0.1106 | 0.0534 |    1872 B |        1.89 |
| &#39;Validate: SCC agreement&#39;                 | 6.311 μs | 3.3564 μs | 0.1840 μs |  3.34 |    0.10 |    3 | 0.1526 | 0.0763 |    2640 B |        2.66 |
| &#39;Validate: TIA (deep cascade)&#39;            | 7.141 μs | 3.9555 μs | 0.2168 μs |  3.78 |    0.12 |    3 | 0.1755 | 0.0916 |    2960 B |        2.98 |
| &#39;Validate: block (full cascade)&#39;          | 5.356 μs | 9.5864 μs | 0.5255 μs |  2.83 |    0.25 |    3 | 0.1755 | 0.0839 |    2944 B |        2.97 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 988.8 ns | 441.9 ns | 24.22 ns |  1.00 |    0.03 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 976.6 ns | 935.9 ns | 51.30 ns |  0.99 |    0.05 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 871.0 ns | 270.8 ns | 14.84 ns |  0.88 |    0.02 |    2 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetActiveDPA by processor ID&#39;            | 863.3 ns | 413.2 ns | 22.65 ns |  0.87 |    0.03 |    2 | 0.0248 | 0.0238 |     440 B |        1.25 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         | 703.1 ns | 270.9 ns | 14.85 ns |  0.71 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |

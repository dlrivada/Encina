```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         |   984.0 ns | 278.6 ns | 15.27 ns |  1.00 |    0.02 |    2 | 0.0210 | 0.0200 |     368 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,045.6 ns | 987.8 ns | 54.14 ns |  1.06 |    0.05 |    2 | 0.0248 | 0.0229 |     424 B |        1.15 |
| &#39;GetDPA by ID (cached read)&#39;              |   990.1 ns | 189.7 ns | 10.40 ns |  1.01 |    0.02 |    2 | 0.0248 | 0.0238 |     440 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            |   938.2 ns | 285.6 ns | 15.65 ns |  0.95 |    0.02 |    2 | 0.0248 | 0.0229 |     424 B |        1.15 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   770.6 ns | 240.0 ns | 13.16 ns |  0.78 |    0.02 |    1 | 0.0229 | 0.0219 |     400 B |        1.09 |

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean       | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-----------:|-----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;HasValidDPA (pipeline hot-path)&#39;         | 1,270.6 ns | 1,411.3 ns | 77.36 ns |  1.00 |    0.07 |    2 | 0.0210 | 0.0191 |     352 B |        1.00 |
| &#39;ValidateDPA (detailed compliance check)&#39; | 1,276.5 ns |   585.6 ns | 32.10 ns |  1.01 |    0.06 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetDPA by ID (cached read)&#39;              | 1,224.9 ns |   242.6 ns | 13.30 ns |  0.97 |    0.05 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetActiveDPA by processor ID&#39;            | 1,176.6 ns |   189.8 ns | 10.40 ns |  0.93 |    0.05 |    2 | 0.0248 | 0.0229 |     424 B |        1.20 |
| &#39;GetExpiringDPAs (filtered scan)&#39;         |   948.9 ns |   468.1 ns | 25.66 ns |  0.75 |    0.04 |    1 | 0.0229 | 0.0219 |     400 B |        1.14 |

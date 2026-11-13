```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 4,281.1 ns |   676.9 ns |  37.11 ns |  1.00 |    0.01 |    3 | 0.0992 | 0.0458 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,524.3 ns | 2,380.5 ns | 130.48 ns |  0.59 |    0.03 |    2 | 0.0648 | 0.0305 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   978.7 ns |   494.8 ns |  27.12 ns |  0.23 |    0.01 |    1 | 0.0210 | 0.0191 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1,070.6 ns | 1,153.3 ns |  63.22 ns |  0.25 |    0.01 |    1 | 0.0229 | 0.0210 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; |   904.4 ns |   347.0 ns |  19.02 ns |  0.21 |    0.00 |    1 | 0.0210 | 0.0200 |     376 B |        0.21 |

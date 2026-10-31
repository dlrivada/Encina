```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,180.4 ns |   535.85 ns | 29.37 ns |  1.00 |    0.01 |    3 | 0.1030 | 0.0496 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 1,932.1 ns | 1,046.08 ns | 57.34 ns |  0.61 |    0.02 |    2 | 0.0648 | 0.0305 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   745.0 ns |   204.46 ns | 11.21 ns |  0.23 |    0.00 |    1 | 0.0219 | 0.0210 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; |   768.5 ns |   164.62 ns |  9.02 ns |  0.24 |    0.00 |    1 | 0.0229 | 0.0219 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   669.1 ns |    71.28 ns |  3.91 ns |  0.21 |    0.00 |    1 | 0.0210 | 0.0200 |     376 B |        0.21 |

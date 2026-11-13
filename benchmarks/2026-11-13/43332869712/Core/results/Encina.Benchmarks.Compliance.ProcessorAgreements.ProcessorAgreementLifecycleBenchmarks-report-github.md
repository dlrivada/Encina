```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 3,644.4 ns | 3,938.5 ns | 215.88 ns |  1.00 |    0.07 |    3 | 0.1030 | 0.0496 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,040.8 ns |   634.1 ns |  34.75 ns |  0.56 |    0.03 |    2 | 0.0648 | 0.0324 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   777.4 ns |   248.5 ns |  13.62 ns |  0.21 |    0.01 |    1 | 0.0219 | 0.0210 |     384 B |        0.22 |
| &#39;RenewDPA (extend expiration)&#39; |   889.5 ns | 1,470.9 ns |  80.62 ns |  0.24 |    0.02 |    1 | 0.0229 | 0.0219 |     408 B |        0.23 |
| &#39;TerminateDPA (end agreement)&#39; |   731.4 ns |   571.6 ns |  31.33 ns |  0.20 |    0.01 |    1 | 0.0210 | 0.0200 |     376 B |        0.21 |

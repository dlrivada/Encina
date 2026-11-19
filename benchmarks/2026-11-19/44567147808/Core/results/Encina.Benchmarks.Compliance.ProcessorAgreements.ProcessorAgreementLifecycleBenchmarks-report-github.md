```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error    | StdDev   | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 4,166.5 ns | 656.7 ns | 36.00 ns |  1.00 |    3 | 0.0992 | 0.0458 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,422.1 ns | 292.7 ns | 16.04 ns |  0.58 |    2 | 0.0648 | 0.0305 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      |   974.5 ns | 439.5 ns | 24.09 ns |  0.23 |    1 | 0.0210 | 0.0191 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1,040.4 ns | 217.5 ns | 11.92 ns |  0.25 |    1 | 0.0229 | 0.0210 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; |   892.4 ns | 244.5 ns | 13.40 ns |  0.21 |    1 | 0.0210 | 0.0200 |     376 B |        0.21 |

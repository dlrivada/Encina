```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 0.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;ExecuteDPA (new agreement)&#39;   | 4,026.7 ns |   869.4 ns |  47.65 ns |  1.00 |    0.01 |    3 | 0.0191 | 0.0153 |    1776 B |        1.00 |
| &#39;AmendDPA (update terms)&#39;      | 2,537.5 ns |   996.1 ns |  54.60 ns |  0.63 |    0.01 |    2 | 0.0114 | 0.0076 |    1104 B |        0.62 |
| &#39;AuditDPA (record audit)&#39;      | 1,169.1 ns | 3,510.9 ns | 192.44 ns |  0.29 |    0.04 |    1 | 0.0038 | 0.0019 |     368 B |        0.21 |
| &#39;RenewDPA (extend expiration)&#39; | 1,168.3 ns | 2,783.6 ns | 152.58 ns |  0.29 |    0.03 |    1 | 0.0038 | 0.0019 |     392 B |        0.22 |
| &#39;TerminateDPA (end agreement)&#39; |   982.7 ns |   284.2 ns |  15.58 ns |  0.24 |    0.00 |    1 | 0.0038 | 0.0019 |     360 B |        0.20 |

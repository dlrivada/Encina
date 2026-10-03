```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|-------------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,327.4 ns |  2,006.37 ns | 109.98 ns |  1.00 |    0.02 |    5 |  0.2289 | 0.0916 |    4224 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,586.3 ns | 10,089.42 ns | 553.04 ns |  1.03 |    0.06 |    5 |  0.2136 | 0.0763 |    3688 B |        0.87 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,510.7 ns |    410.99 ns |  22.53 ns |  0.30 |    0.00 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,421.3 ns |  1,413.18 ns |  77.46 ns |  0.17 |    0.01 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,445.0 ns |  3,172.65 ns | 173.90 ns |  0.53 |    0.02 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     736.3 ns |     15.26 ns |   0.84 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 180,597.0 ns | 12,037.38 ns | 659.81 ns | 21.69 |    0.26 |    6 | 17.3340 | 0.4883 |  291296 B |       68.96 |

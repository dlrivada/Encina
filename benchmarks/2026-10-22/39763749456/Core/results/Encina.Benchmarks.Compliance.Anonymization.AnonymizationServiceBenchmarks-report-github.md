```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|-------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9,296.0 ns |  5,499.90 ns |   301.47 ns |  1.00 |    0.04 |    5 |  0.2136 | 0.0763 |    3624 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   9,070.1 ns |  3,152.16 ns |   172.78 ns |  0.98 |    0.03 |    5 |  0.2594 | 0.1068 |    4887 B |        1.35 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,709.7 ns |    934.04 ns |    51.20 ns |  0.29 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.17 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,479.3 ns |    295.76 ns |    16.21 ns |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,490.4 ns |    341.47 ns |    18.72 ns |  0.48 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     796.2 ns |     77.03 ns |     4.22 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.26 |
| &#39;Risk assessment: 100-record dataset&#39;     | 184,080.7 ns | 26,471.67 ns | 1,451.00 ns | 19.82 |    0.58 |    6 | 17.3340 | 0.4883 |  291296 B |       80.38 |

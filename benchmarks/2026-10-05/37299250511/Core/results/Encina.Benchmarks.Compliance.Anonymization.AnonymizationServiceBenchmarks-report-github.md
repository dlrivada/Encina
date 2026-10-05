```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|--------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,692.6 ns |   1,307.86 ns |    71.69 ns |  1.00 |    0.01 |    5 |  0.2289 | 0.0916 |    4222 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,776.5 ns |   4,569.64 ns |   250.48 ns |  1.01 |    0.03 |    5 |  0.2441 | 0.0916 |    4283 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,633.7 ns |   2,124.37 ns |   116.44 ns |  0.30 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,395.9 ns |      36.84 ns |     2.02 ns |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,271.4 ns |     570.12 ns |    31.25 ns |  0.49 |    0.00 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     780.2 ns |     411.50 ns |    22.56 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 190,066.3 ns | 128,036.24 ns | 7,018.10 ns | 21.87 |    0.72 |    6 | 17.3340 | 0.4883 |  291296 B |       68.99 |

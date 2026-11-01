```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   9,925.7 ns |   347.12 ns |   497.83 ns |  1.00 |    0.07 |    5 |  0.1984 | 0.0610 |    3504 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |  10,522.2 ns |   488.99 ns |   685.50 ns |  1.06 |    0.09 |    5 |  0.2136 | 0.0763 |    3584 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,655.3 ns |    38.40 ns |    57.47 ns |  0.27 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,425.5 ns |    19.74 ns |    28.94 ns |  0.14 |    0.01 |    2 |  0.0229 |      - |     392 B |        0.11 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,361.5 ns |    33.18 ns |    47.58 ns |  0.44 |    0.02 |    4 |  0.0610 |      - |    1048 B |        0.30 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     786.2 ns |     7.95 ns |    11.66 ns |  0.08 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.27 |
| &#39;Risk assessment: 100-record dataset&#39;     | 179,464.4 ns | 1,564.92 ns | 2,293.84 ns | 18.12 |    0.90 |    6 | 17.3340 | 0.4883 |  291296 B |       83.13 |

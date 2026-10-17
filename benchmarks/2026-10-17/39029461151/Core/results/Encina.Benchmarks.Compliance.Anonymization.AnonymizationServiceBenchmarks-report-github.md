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
| &#39;Tokenize: UUID format&#39;                   |   8,340.5 ns |  2,780.60 ns |   152.41 ns |  1.00 |    0.02 |    5 |  0.2289 | 0.0763 |    4226 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,318.7 ns |    738.60 ns |    40.49 ns |  1.00 |    0.02 |    5 |  0.2441 | 0.0916 |    4289 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,698.1 ns |    551.51 ns |    30.23 ns |  0.32 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,362.5 ns |     88.17 ns |     4.83 ns |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,443.2 ns |  1,391.76 ns |    76.29 ns |  0.53 |    0.01 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     735.3 ns |     18.59 ns |     1.02 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 178,465.2 ns | 19,565.44 ns | 1,072.45 ns | 21.40 |    0.35 |    6 | 17.3340 | 0.4883 |  291296 B |       68.93 |

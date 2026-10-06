```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|-----------:|----------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,955.7 ns | 3,700.3 ns | 202.83 ns |  1.00 |    0.03 |    5 |  0.2289 | 0.0916 |    4223 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   9,414.8 ns | 5,864.9 ns | 321.47 ns |  1.05 |    0.04 |    5 |  0.2441 | 0.1068 |    4285 B |        1.01 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,694.4 ns | 1,647.6 ns |  90.31 ns |  0.30 |    0.01 |    3 |  0.0343 |      - |     632 B |        0.15 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,422.9 ns |   176.8 ns |   9.69 ns |  0.16 |    0.00 |    2 |  0.0229 |      - |     384 B |        0.09 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,621.8 ns | 6,209.7 ns | 340.37 ns |  0.52 |    0.03 |    4 |  0.0610 |      - |    1088 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     764.8 ns |   217.9 ns |  11.95 ns |  0.09 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.22 |
| &#39;Risk assessment: 100-record dataset&#39;     | 183,169.8 ns | 9,900.2 ns | 542.66 ns | 20.46 |    0.40 |    6 | 17.3340 | 0.4883 |  291296 B |       68.98 |

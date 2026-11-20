```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|-------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,960.2 ns |  6,734.87 ns |   369.16 ns |  1.00 |    0.05 |    5 |  0.2289 | 0.0916 |    4098 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,966.0 ns |  5,626.38 ns |   308.40 ns |  1.00 |    0.05 |    5 |  0.2289 | 0.0916 |    4169 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,466.5 ns |    588.44 ns |    32.25 ns |  0.28 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.12 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,360.2 ns |    153.32 ns |     8.40 ns |  0.15 |    0.01 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,321.5 ns |    162.13 ns |     8.89 ns |  0.48 |    0.02 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     881.4 ns |     92.88 ns |     5.09 ns |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 212,599.6 ns | 30,174.59 ns | 1,653.97 ns | 23.75 |    0.86 |    6 | 17.3340 | 0.4883 |  291296 B |       71.08 |

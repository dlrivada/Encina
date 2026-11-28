```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                    | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------ |-------------:|------------:|------------:|------:|--------:|-----:|--------:|-------:|----------:|------------:|
| &#39;Tokenize: UUID format&#39;                   |   8,792.7 ns |  7,337.3 ns |   402.18 ns |  1.00 |    0.06 |    5 |  0.2289 | 0.1068 |    4087 B |        1.00 |
| &#39;Tokenize: prefixed format&#39;               |   8,610.5 ns |  9,065.1 ns |   496.89 ns |  0.98 |    0.06 |    5 |  0.2289 | 0.0916 |    4169 B |        1.02 |
| &#39;Pseudonymize: AES-256-GCM&#39;               |   2,289.0 ns |  1,266.3 ns |    69.41 ns |  0.26 |    0.01 |    3 |  0.0305 |      - |     512 B |        0.13 |
| &#39;Pseudonymize: HMAC-SHA256&#39;               |   1,281.6 ns |    535.3 ns |    29.34 ns |  0.15 |    0.01 |    2 |  0.0229 |      - |     392 B |        0.10 |
| &#39;Pseudonymize + Depseudonymize roundtrip&#39; |   4,005.0 ns |  2,166.0 ns |   118.73 ns |  0.46 |    0.02 |    4 |  0.0610 |      - |    1048 B |        0.26 |
| &#39;Anonymize: data masking (2 fields)&#39;      |     858.2 ns |    550.7 ns |    30.19 ns |  0.10 |    0.00 |    1 |  0.0553 |      - |     936 B |        0.23 |
| &#39;Risk assessment: 100-record dataset&#39;     | 205,713.3 ns | 86,767.4 ns | 4,756.01 ns | 23.43 |    1.02 |    6 | 17.3340 | 0.4883 |  291296 B |       71.27 |
